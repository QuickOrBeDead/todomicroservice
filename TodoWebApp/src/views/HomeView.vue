<template>
  <div class="home">
    <img alt="Vue logo" src="../assets/logo.png">
    <HelloWorld msg="Welcome to Your Vue.js + TypeScript App"/>

    <li v-for="task in tasks" :key="task.id">
      {{ task.title }}
    </li>
  </div>
</template>

<script lang="ts">
import { Options, Vue } from 'vue-class-component';
import HelloWorld from '@/components/HelloWorld.vue'; // @ is an alias to /src
import { createTaskManagementApi } from '@/api'
import { TaskApi, TaskListItemViewModel } from '@/metadata/task-management-api';

@Options({
  components: {
    HelloWorld,
  },
})
export default class HomeView extends Vue {
  taskApi: TaskApi = createTaskManagementApi(TaskApi)
  tasks: TaskListItemViewModel[] = []

  listTasks(): void {
    this.taskApi.getTasks().then(x => this.tasks = x.data)
  }

  mounted(): void {
      this.listTasks()
  }
}
</script>
